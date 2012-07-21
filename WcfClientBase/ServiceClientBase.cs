﻿using System;
using System.ServiceModel;

namespace WcfClientBase
{

    /// <summary>
    /// Provides the base implementation used to open, close and abort service channels and provides
    /// callbacks to handle exceptions.
    /// </summary>
    /// <typeparam name="TServiceClient">The type that is identified as ServiceClient generated by WCF</typeparam>
    public abstract class ServiceClientBase<TServiceClient> where TServiceClient : ICommunicationObject, new()
    {
        /// <summary>
        /// Optional timeout value for closing channel
        /// </summary>
        protected TimeSpan? CloseTimeout { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the TServiceClient using parameterless constructor.
        /// Override if you need to use one of the other constructors.
        /// Called before every service operation.
        /// </summary>
        protected virtual TServiceClient InitializeServiceClient()
        {
            return new TServiceClient();
        }

        /// <summary>
        /// Generic method for initializing ServiceClient, performing service operation, closing the channel or 
        /// handling the exception and aborting the channel.
        /// </summary>
        /// <param name="serviceCall">Callback for assigning service operation response</param>
        private void HandleServiceCall(Action<TServiceClient> serviceCall)
        {
            TServiceClient serviceClient = InitializeServiceClient();

            try
            {
                serviceCall.Invoke(serviceClient);
                
                if(CloseTimeout.HasValue)
                {
                    serviceClient.Close(CloseTimeout.Value);
                }
                else
                {
                    serviceClient.Close();
                }
            }
            catch (FaultException exception)
            {
                serviceClient.Abort();
                HandleFaultException(exception);
            }
            catch (CommunicationException exception)
            {
                serviceClient.Abort();
                HandleCommunicationException(exception);
            }
            catch (TimeoutException exception)
            {
                serviceClient.Abort();
                HandleTimeoutException(exception);
            }

        }

        /// <summary>
        /// Performs a service operation and returns the response from that operation.
        /// </summary>
        /// <typeparam name="TResponse">Service operation return type</typeparam>
        /// <param name="serviceMethod">Service method call</param>
        /// <returns></returns>
        protected TResponse PerformServiceOperation<TResponse>(Func<TServiceClient, TResponse> serviceMethod)
        {
            TResponse result = default(TResponse);
            HandleServiceCall(item => result = serviceMethod.Invoke(item));

            return result;
        }

        /// <summary>
        /// Performs a service operation that does not return anything.
        /// </summary>
        /// <param name="serviceMethod">Service method call</param>
        protected void PerformServiceOperation(Action<TServiceClient> serviceMethod)
        {
            HandleServiceCall(serviceMethod);
        }

        /// <summary>
        /// Tries to perform a service operation and return the response from that operation.
        /// Returns false if an exception is thrown.
        /// </summary>
        /// <typeparam name="TResponse">Service operation return type</typeparam>
        /// <param name="serviceMethod">Service method call</param>
        /// <param name="result">Result that was returned from the service
        /// Default of TResponse if operation failed.</param>
        /// <returns></returns>
        protected bool TryPerformServiceOperation<TResponse>(Func<TServiceClient, TResponse> serviceMethod, out TResponse result)
        {
            bool isOperationSuccessful = false;
            TResponse serviceResponse = default(TResponse);

            HandleServiceCall(item =>
                                  {
                                      serviceResponse = serviceMethod.Invoke(item);
                                      isOperationSuccessful = true;
                                  });
            
            result = serviceResponse;
            return isOperationSuccessful;
        }

        /// <summary>
        /// Tries to perform a service operation that does not return anything.
        /// IsOperationSuccessful is false if an exception is thrown.
        /// </summary>
        /// <param name="serviceMethod">Service method call</param>
        /// <returns></returns>
        protected bool TryPerformServiceOperation(Action<TServiceClient> serviceMethod)
        {
            bool result = false;

            HandleServiceCall(item =>
                                  {
                                      serviceMethod.Invoke(item);
                                      result = true;
                                  });

            return result;
        }

        /// <summary>
        /// Method for handling FaultException
        /// </summary>
        /// <param name="exception">Exception that was thrown by the ServiceClient</param>
        protected virtual void HandleFaultException(FaultException exception)
        {
            throw exception;
        }

        /// <summary>
        /// Method for handling CommunicationException
        /// </summary>
        /// <param name="exception">Exception that was thrown by the ServiceClient</param>
        protected virtual void HandleCommunicationException(CommunicationException exception)
        {
            throw exception;
        }

        /// <summary>
        /// Method for handling TimeoutException
        /// </summary>
        /// <param name="exception">Exception that was thrown by the ServiceClient</param>
        protected virtual void HandleTimeoutException(TimeoutException exception)
        {
            throw exception;
        }

    }
}